import { Hono } from "hono";
import { CheckinController } from "../controllers/checkin.controller";

const checkinRoute = new Hono<{ Bindings: CloudflareBindings }>();

checkinRoute.get("/qrcode", CheckinController.getQRCode);
checkinRoute.get("/result", CheckinController.getResult);

export default checkinRoute;

