sudo openssl req -x509 -nodes -days 3650 -newkey rsa:4096 -keyout matrixease.key -out matrixease.crt -config matrixease_cert.conf

sudo mkdir /etc/nginx/ssl
sudo cp matrixease.crt /etc/nginx/ssl/matrixease.crt
sudo cp matrixease.key /etc/nginx/ssl/matrixease.key

sudo systemctl restart nginx

