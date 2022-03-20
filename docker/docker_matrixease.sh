#!/bin/bash
op=$1

if [ "$op" == "start" ]; then
    OP=start
fi


if [ "$op" == "stop" ]; then
    OP=stop
fi

cd ~/docker/matrixease_blog_wp
echo "matrixease_blog_wp $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/matrixease_docs_static
echo "vi matrixease_docs_static $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/matrixease_dotnet
echo "matrixease_dotnet $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/matrixease_static
echo "matrixease_static $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/matrixease_support_wp
echo "matrixease_support_wp $OP"
sudo docker-compose $OP
echo ""

cd ~/docker

