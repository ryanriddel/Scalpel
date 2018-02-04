#!/bin/bash
sudo docker run -d -p 9000:80 -v $PWD/genconf/serve:/usr/share/nginx/html:ro nginx