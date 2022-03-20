#!/bin/bash
op=$1

if [ "$op" == "start" ]; then
    OP=start
fi


if [ "$op" == "stop" ]; then
    OP=stop
fi

cd ~/docker/visalyzer_blog_wp
echo "visalyzer_blog_wp $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/visalyzer_docs_static
echo "vi visalyzer_docs_static $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/visalyzer_dotnet
echo "visalyzer_dotnet $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/visalyzer_static
echo "visalyzer_static $OP"
sudo docker-compose $OP
echo ""

cd ~/docker/visalyzer_support_wp
echo "visalyzer_support_wp $OP"
sudo docker-compose $OP
echo ""

cd ~/docker

