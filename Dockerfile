FROM nginx:alpine
COPY wwwroot/ /usr/share/nginx/html/
EXPOSE 8080
RUN sed -i 's/listen       80;/listen       8080;/' /etc/nginx/conf.d/default.conf
