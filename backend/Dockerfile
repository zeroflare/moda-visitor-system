FROM node:20

WORKDIR /app

COPY package*.json ./
RUN npm ci
COPY . .

EXPOSE 80

CMD ["npx", "wrangler", "dev", "--local", "--port", "80", "--ip", "0.0.0.0"]

