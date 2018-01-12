FROM centos:latest

MAINTAINER Ryan Riddel (r_riddel@scalptrade.com)

#create a nice centos

RUN mkdir /build

ADD build/epel /build/epel
RUN /build/epel/install.sh 

ADD build/runit /build/runit
RUN /build/runit/install.sh

ADD build/sshd /build/sshd
RUN /build/sshd/install.sh

ADD build/rsyslogd /build/rsyslogd
RUN /build/rsyslogd/install.sh

#RUN /build/clean.sh

#Install dev tools
RUN yum update
RUN yum install -y screen
RUN yum install -y cmake
RUN yum install -y pkg-config
RUN yum install -y vim
RUN yum install -y vi
RUN yum install -y git
RUN yum install -y python
RUN yum install -y curl

#Install GO
RUN curl https://go.googlecode.com/files/go1.2.1.linux-amd64.tar.gz | tar -C /usr/local -zx
ENV GOROOT /usr/local/go
ENV PATH /usr/local/go/bin:$PATH


#Create shared data volume
RUN mkdir /var/shared/
RUN touch /var/shared/placeholder
RUN chown -R dev:dev /var/shared
VOLUME /var/shared

# Link in shared parts of the home directory
RUN ln -s /var/shared/.ssh
RUN ln -s /var/shared/.bash_history
RUN ln -s /var/shared/.maintainercfg


EXPOSE 80
EXPOSE 4222
EXPOSE 8222


#Copy oprabk-specific files from host 

COPY  /home/scalp/opra/oprabk /oprabk
COPY  /home/scalp/opra/oprabkapp /oprabkapp

COPY  /home/scalp/opra/oprasdb /oprasdb
COPY  /home/scalp/opra/opnintdb /opnintdb
COPY  /home/scalp/opra/opnintdbapp /opnintdbapp
COPY  /home/scalp/opra/secudbapp /secudbapp
COPY  /home/scalp/opra/optdb /optdb

RUN mkdir -p /home/scalp/script
RUN mkdir -p /logs

COPY  /home/scalp/script/startup /home/scalp/script/startup 

#COPY  /home/scalp/opra/streaminterfaceapp /streaminterfaceapp
#COPY /home/scalp/opra/quoteprotostream /quoteprotostream
#COPY /home/scalp/opra/tradeprotostream /tradeprotostream 

COPY /usr/local/lib /usr/local/lib

RUN chmod u+x /oprabk && chmod u+x /oprabkapp
RUN chmod u+x /oprasdb && chmod u+x /opnintdb
RUN chmod u+x /opnintdbapp && chmod u+x /secudbapp
RUN chmod u+x /optdb && chmod u+x /home/scalp/script/startup
#RUN chmod u+x /streaminterfaceapp && chmod u+x /quoteprotostream && chmod u+x /tradeprotostream


CMD ["oprabk", "start"]
