#!/bin/bash
op=$1

if [ "$op" == "start" ]; then
    OP=start
fi


if [ "$op" == "stop" ]; then
    OP=stop
fi

cd ~/docker/matrixease_dotnet
echo "matrixease_dotnet $OP"
sudo docker compose $OP
echo ""

cd ~/docker
