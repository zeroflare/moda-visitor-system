import { Hono } from "hono";
import { RegisterController } from "../controllers/register.controller";

const registerRoute = new Hono<{ Bindings: CloudflareBindings }>();

registerRoute.post("/otp", RegisterController.sendOTP);
registerRoute.post("/qrcode", RegisterController.submitRegistration);
registerRoute.get("/result", RegisterController.getRegistrationResult);

export default registerRoute;

