# editor-monitoring



## Getting started
`docker run -e SMTP_HOST=smtp.yourdomain.com \
           -e SMTP_PORT=587 \
           -e SMTP_USER=your-smtp-username \
           -e SMTP_PASSWORD=your-smtp-password \
           -e SMTP_ENABLE_SSL=true \
           -p 3000:3000 -p 5000:5000 your-docker-image`
