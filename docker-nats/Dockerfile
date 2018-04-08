FROM centos:centos7
#FROM alpine:3.4

MAINTAINER Ryan Riddel (r_riddel@scalptrade.com)


WORKDIR /


#Install dev tools
#RUN yum install -y bash
RUN yum update -y  
#RUN yum install -y cmake vim git curl glibc-headers gcc-c++ autoconf automake libtool unzip make gdb

#Install GO
#RUN curl https://go.googlecode.com/files/go1.2.1.linux-amd64.tar.gz | tar -C /usr/local -zx
#ENV GOROOT /usr/local/go
#ENV PATH /usr/local/go/bin:$PATH

RUN yum install -y epel-release nodejs
RUN yum install -y npm
RUN npm install natsboard -g


EXPOSE 80
EXPOSE 4222
EXPOSE 7244
EXPOSE 8222
EXPOSE 3000

COPY ./run-gnatsd-node /run-gnatsd-node
COPY ./gnatsd-node-config-dcos /gnatsd-node-config-dcos
COPY ./gnatsd /gnatsd
#RUN mkdir /bin
#COPY ./sh /bin

CMD "/run-gnatsd-node"
#CMD ["/gnatsd, "-c", "gnatsd-node-config-dcos"]
#CMD ["bin/sh","-c"]
#CMD ["/scalp/tiller/tiller", "-b", "/scalp/tiller/quickstart", , "-d", "-a", "-v", "--environment", $(hostname)]


