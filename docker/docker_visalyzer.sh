#!/bin/bash
op=$1

if [ "$op" == "start" ]; then
    OP=start
fi

if [ "$op" == "stop" ]; then
    OP=stop
fi

cd ~/docker/visalyzer_blog_wp
sudo docker-compose $OP

cd ~/docker/visalyzer_docs_static
sudo docker-compose $OP

cd ~/docker/visalyzer_dotnet
sudo docker-compose $OP

cd ~/docker/visalyzer_static
sudo docker-compose $OP

cd ~/docker/visalyzer_support_wp
sudo docker-compose $OP

cd ~/docker
