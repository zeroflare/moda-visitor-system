import { Hono } from "hono";
import messageRoute from "./message.route";
import checkinRoute from "./checkin.route";
import registerRoute from "./register.route";

const apiRoutes = new Hono<{ Bindings: CloudflareBindings }>();

apiRoutes.route("/message", messageRoute);
apiRoutes.route("/checkin", checkinRoute);
apiRoutes.route("/register", registerRoute);

export default apiRoutes;

