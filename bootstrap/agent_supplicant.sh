#!/bin/bash

#we need to setup the ssh authorized file
file="/home/vagrant/genconf/config.yaml"
while  IFS= read -r  line; do

readFlag=true

if [ "$line" =  "master_list:" ]
then
echo "masters:"
       while $readFlag
       do
        read -r ipaddr
        echo "$ipaddr"
        #ssh into each master and do stuff
        IFS=' ' read -r -a array <<< "$ipaddr"

        if [ "$array[0]" = "-" ]
        then
                echo "yes! $array[0]"
        else
                readFlag=false
        fi

       done < "$file"

elif [ "$line" =  "public_agent_list:" ]
then
        echo "agents:"

     while $readFlag
       do
         read -r ipaddr
         #ssh into each agent  and do stuff
        IFS=' ' read -r -a array <<< "$ipaddr"

         if [ "$array[0]" = "-" ]
         then
                 echo "yes! $array[0]"
         else
                 readFlag=false
        fi

     done < "$file"

fi
done < "$file"