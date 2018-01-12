#!/usr/bin/env bash
cd $(dirname $0)
docker run -rm -i -t -v /var/run/docker.sock:/var/run/docker.sock docker-oprabk $@
