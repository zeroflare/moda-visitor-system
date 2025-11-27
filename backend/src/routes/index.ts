import { Hono } from "hono";
import messageRoute from "./message.route";
import checkinRoute from "./checkin.route";

const apiRoutes = new Hono<{ Bindings: CloudflareBindings }>();

apiRoutes.route("/message", messageRoute);
apiRoutes.route("/checkin", checkinRoute);

export default apiRoutes;

