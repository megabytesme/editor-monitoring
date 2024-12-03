FROM node:latest AS builder
WORKDIR /app
COPY /src/monitor-frontend/package*.json ./
RUN npm install
COPY /src/monitor-frontend/. .
RUN npm run build

FROM node:alpine
WORKDIR /app
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/package.json ./package.json

RUN npm install next

CMD ["npm", "start"]