import type { Context } from "hono";

export class CheckinController {
  static async getQRCode(c: Context) {
    const { env } = c;
    const apiUrl = env.TWDIW_VP_URL;
    const token = env.TWDIW_VP_TOKEN;
    const ref = env.TWDIW_VP_ID_CHECKIN;
    const transactionId = c.req.query("transactionId");
    if (!transactionId || !CheckinController.isUUID(transactionId)) {
      return c.json(
        {
          error: true,
          message: "transactionId 格式錯誤，需為 UUID",
        },
        400
      );
    }

    const url = `${apiUrl}/api/oidvp/qrcode?ref=${ref}&transactionId=${transactionId}`;
    const res = await fetch(url, {
      headers: {
        "Access-Token": token,
      },
    });

    if (!res.ok) {
      return c.json(
        {
          error: true,
          message: "取得 QR Code 失敗",
        },
        500
      );
    }

    const data = (await res.json()) as {
      qrcodeImage: string;
      authUri: string;
    };

    return c.json({
      qrcodeImage: data.qrcodeImage,
      authUri: data.authUri,
    });
  }

  static async getResult(c: Context) {
    try {
      const { env, req } = c;
      const apiUrl = env.TWDIW_VP_URL;
      const transactionId = req.query("transactionId");

      if (!transactionId || !CheckinController.isUUID(transactionId)) {
        return c.json(
          {
            error: true,
            message: "transactionId 格式錯誤，需為 UUID",
          },
          400
        );
      }

      const url = `${apiUrl}/api/oidvp/result`;
      const res = await fetch(url, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Access-Token": env.TWDIW_VP_TOKEN,
        },
        body: JSON.stringify({ transactionId }),
      });

      const text = await res.text();

      if (!res.ok) {
        console.log("loginResult error:", text);
        return c.json({ message: "等待驗證" }, 400);
      }

      const data = JSON.parse(text);

      const claims = data.data?.[0]?.claims ?? [];
      const email = claims.find((claim: any) => claim.ename === "email")?.value;
      const name = claims.find((claim: any) => claim.ename === "name")?.value;
      const company = claims.find((claim: any) => claim.ename === "company")?.value;
      const phone = claims.find((claim: any) => claim.ename === "phone")?.value;

   
      return c.json({ 
        inviterEmail: "a@moda.gov.tw",
        inviterName: "邀請者姓名",
        inviterDept: "邀請者單位",
        inviterTitle: "邀請者職稱",
        vistorEmail: email,
        vistorName: name,
        vistorDept: company,
        vistorPhone: phone,
        meetingTime: "2025-11-27 10:00:00",
        meetingRoom: "延平 201 會議室",
      });
    } catch (error) {
      console.error("loginResult exception:", error);
      return c.json({ error: true, message: "發生錯誤" }, 500);
    }
  }

  private static isUUID(value: string) {
    const uuidRegex =
      /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    return uuidRegex.test(value);
  }
}

