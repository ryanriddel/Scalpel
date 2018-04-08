FROM centos:7

MAINTAINER Ryan Riddel (r_riddel@scalptrade.com)

WORKDIR /
#Install dev tools
RUN yum update -y  
RUN yum install -y cmake vim git curl glibc-headers gcc-c++ autoconf automake libtool unzip make gdb lsof-4.87-4.el7.x86_64 net-tools-2.0-0.22.20131004git.el7.x86_64 


#EXPOSE 80
EXPOSE 4222
EXPOSE 8222


#Copy csbk-specific files from host 
WORKDIR /
RUN mkdir -p /scalp && mkdir -p /scalp/data
COPY  csbk2 /scalp/csbk2
COPY  csbkapp2 /scalp/csbkapp2

RUN mkdir -p /scalp/script && mkdir -p /scalp/tiller/quickstart/templates
RUN mkdir -p /scalp/logs

COPY startup /scalp/script/startup 
COPY startall /scalp/startall

COPY sdbadmin /scalp/sdbadmin

COPY streaminterfaceapp /scalp/streaminterfaceapp
COPY quoteprotostream /scalp/quoteprotostream
COPY tradeprotostream /scalp/tradeprotostream 

#COPY usr/local/lib /usr/local/lib

RUN chmod u+x /scalp/sdbadmin
RUN chmod u+x /scalp/script/startup && chmod u+x /scalp/startall
RUN chmod u+x /scalp/streaminterfaceapp && chmod u+x /scalp/quoteprotostream && chmod u+x /scalp/tradeprotostream

RUN yum install -y wget ruby-2.0.0.648-30.el7.x86_64 
RUN gem install tiller 

COPY tiller /scalp/tiller



#CMD ["tiller", "-b", "/scalp/tiller/quickstart", "-d", "-a", "-v","--environment", $(hostname)]


CMD tiller -b /scalp/tiller/quickstart -a -v --environment $(hostname)



