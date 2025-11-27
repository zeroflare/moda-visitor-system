import type { Context } from "hono";

export const getMessage = (c: Context) => {
  return c.json({ message: "Hello Hono!" });
};

