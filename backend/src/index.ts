import { Hono } from "hono";
import apiRoutes from "./routes";
import { requestLogger } from "./middlewares/logger.middleware.js";

const app = new Hono<{ Bindings: CloudflareBindings }>();

app.use("*", requestLogger);
app.route("/api", apiRoutes);

export default app;
