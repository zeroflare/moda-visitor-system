import type { Context } from "hono";

export class RegisterController {
  static async sendOTP(c: Context) {
    try {
      const { email } = await c.req.json();

      if (!email) {
        return c.json({ error: "缺少 email 欄位" }, 400);
      }

      const kv = c.env.KV as KVNamespace;

      // 檢查是否在冷卻期內
      const cooldownKey = `cooldown:${email}`;
      const lastSent = await kv.get(cooldownKey);

      if (lastSent) {
        return c.json(
          { error: "請稍後再試，每分鐘僅能寄送一次驗證碼" },
          429
        );
      }

      // 產生六位數 OTP
      const otp = Math.floor(100000 + Math.random() * 900000).toString();

      // 儲存 OTP (10 分鐘有效)
      await kv.put(`otp:${email}`, otp, { expirationTtl: 600 });

      // 寄信
      const apiKey = c.env.MAILGUN_API_KEY;
      const domain = c.env.MAILGUN_DOMAIN;
      const mailgunUrl = `https://api.mailgun.net/v3/${domain}/messages`;

      const body = new URLSearchParams();
      body.append("from", `數位發展部訪客系統<noreply@${domain}>`);
      body.append("to", email);
      body.append("subject", "訪客系統電子信箱驗證碼");
      body.append(
        "text",
        `您好！\n\n您的驗證碼是：${otp}\n請於 10 分鐘內輸入完成註冊。`
      );
      body.append(
        "html",
        `<p>您好！</p><p>您的驗證碼是：<strong>${otp}</strong></p><p>請於 10 分鐘內輸入完成註冊。</p>`
      );

      const response = await fetch(mailgunUrl, {
        method: "POST",
        headers: {
          Authorization: "Basic " + btoa(`api:${apiKey}`),
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body,
      });

      if (!response.ok) {
        const text = await response.text();
        console.error("Mailgun error:", text);
        return c.json({ error: "寄信失敗，請稍後再試" }, 500);
      }

      // 寫入冷卻鍵 (60 秒內不能再寄)
      await kv.put(cooldownKey, "1", { expirationTtl: 60 });

      return c.json({ message: "OTP sent successfully" });
    } catch (err: any) {
      console.error("寄信錯誤:", err);
      return c.json({ error: "伺服器錯誤" }, 500);
    }
  }

  static async submitRegistration(c: Context) {
    try {
      const { env, req } = c;
      const kv = env.KV as KVNamespace;
      const body = await req.json();
      const { name, email, phone, company, otp } = body;

      // --- 基本欄位檢查 ---
      if (!name || !email || !otp || !phone || !company) {
        return c.json(
          { error: "缺少必要欄位 (name, email, otp, phone, company)" },
          400
        );
      }

      // --- 檢查 OTP ---
      const storedOtp = await kv.get(`otp:${email}`);
      if (!storedOtp) {
        return c.json({ error: "驗證碼已失效或未發送" }, 400);
      }

      if (storedOtp !== otp) {
        return c.json({ error: "驗證碼錯誤" }, 400);
      }

      // OTP 驗證成功後，刪除舊的 OTP，避免重複使用
      await kv.delete(`otp:${email}`);

      // --- 呼叫 TWDIW 服務 ---
      const apiUrl = env.TWDIW_VC_URL;
      const url = `${apiUrl}/api/qrcode/data`;

      const res = await fetch(url, {
        method: "POST",
        headers: {
          "Access-Token": env.TWDIW_VC_TOKEN,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          vcUid: env.TWDIW_VC_ID,
          fields: [
            { ename: "name", content: name },
            { ename: "email", content: email },
            { ename: "company", content: company },
            { ename: "phone", content: phone },
          ],
        }),
      });

      if (!res.ok) {
        const text = await res.text();
        console.error("TWDIW 錯誤：", text);
        return c.json({ error: "取得 QR Code 失敗" }, 500);
      }

      const data = (await res.json()) as {
        transactionId: string;
        qrCode: string;
        deepLink: string;
      };

      return c.json({
        message: "Registration successful",
        transactionId: data.transactionId,
        qrcodeImage: data.qrCode,
        authUri: data.deepLink,
      });
    } catch (err) {
      console.error("呼叫 TWDIW 錯誤:", err);
      return c.json({ error: "伺服器錯誤，請稍後再試" }, 500);
    }
  }

  static async getRegistrationResult(c: Context) {
    try {
      const { env, req } = c;
      const apiUrl = env.TWDIW_VC_URL;
      const transactionId = req.query("transactionId");

      if (!transactionId) {
        return c.json(
          {
            error: true,
            message: "transactionId 為必填欄位",
          },
          400
        );
      }

      const url = `${apiUrl}/api/credential/nonce/${transactionId}`;
      const res = await fetch(url, {
        headers: {
          "Access-Token": env.TWDIW_VC_TOKEN,
        },
      });

      const data = (await res.json()) as any;

      if (res.ok) {
        return c.json({
          message: "Registration completed",
          ...data,
        });
      } else {
        return c.json(
          {
            message: "Waiting for registration",
          },
          400
        );
      }
    } catch (error) {
      console.error("getRegistrationResult exception:", error);
      return c.json({ error: true, message: "發生錯誤" }, 500);
    }
  }
}

