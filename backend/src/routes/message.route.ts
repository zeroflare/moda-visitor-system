import { Hono } from "hono";
import { getMessage } from "../controllers/message.controller";

const messageRoute = new Hono<{ Bindings: CloudflareBindings }>();

messageRoute.get("/", getMessage);

export default messageRoute;

