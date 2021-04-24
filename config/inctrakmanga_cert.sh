sudo openssl req -x509 -nodes -days 3650 -newkey rsa:4096 -keyout inctrak_ssc.key -out inctrak_ssc.crt -config inctrakmanga_cert.conf

sudo mkdir /etc/nginx/ssl
sudo cp inctrak_ssc.crt /etc/nginx/ssl/inctrak_ssc.crt
sudo cp inctrak_ssc.key /etc/nginx/ssl/inctrak_ssc.key

sudo systemctl restart nginx

