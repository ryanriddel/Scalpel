FROM centos:7

MAINTAINER Ryan Riddel (r_riddel@scalptrade.com)

#create a nice centos

#RUN mkdir /build

#ADD build/epel /build/epel
#RUN /build/epel/install.sh 
#
#ADD build/runit /build/runit
#RUN /build/runit/install.sh
#
#ADD build/sshd /build/sshd
#RUN /build/sshd/install.sh
#
#ADD build/rsyslogd /build/rsyslogd
#RUN /build/rsyslogd/install.sh

#RUN /build/clean.sh

WORKDIR /
#Install dev tools
RUN yum update -y  
RUN yum install -y cmake vim git curl glibc-headers gcc-c++ autoconf automake libtool unzip make gdb


 

#RUN git clone https://github.com/google/protobuf.git 
#WORKDIR /protobuf
#RUN ./autogen.sh 
#RUN ./configure 
#RUN make
#WORKDIR /
#RUN rm -r protobuf

#Install GO
#RUN curl https://go.googlecode.com/files/go1.2.1.linux-amd64.tar.gz | tar -C /usr/local -zx
#ENV GOROOT /usr/local/go
#ENV PATH /usr/local/go/bin:$PATH


#Create shared data volume
#RUN mkdir /var/shared/
#RUN touch /var/shared/placeholder
#RUN chown -R dev:dev /var/shared
#VOLUME /var/shared

# Link in shared parts of the home directory
#RUN ln -s /var/shared/.ssh
#RUN ln -s /var/shared/.bash_history
#RUN ln -s /var/shared/.maintainercfg


#EXPOSE 80
EXPOSE 4222
EXPOSE 8222


#Copy oprabk-specific files from host 
WORKDIR /
RUN mkdir -p /scalp && mkdir -p /scalp/data
COPY  oprabk-c /scalp/oprabk-c
COPY  oprabk /scalp/oprabk
COPY  oprabkapp-c /scalp/oprabkapp-c
COPY  oprabkapp /scalp/oprabkapp
COPY  oprasdb /scalp/oprasdb
COPY  opnintdb /scalp/opnintdb
COPY  opnintbk /scalp/data/opnintbk
COPY  opnintdbapp /scalp/opnintdbapp
COPY  secudbapp /scalp/secudbapp
COPY  optdb /scalp/optdb
COPY  RBConfig.json /scalp/RBConfig.json
RUN mkdir -p /scalp/script && mkdir -p /scalp/tiller/quickstart/templates
RUN mkdir -p /scalp/logs

COPY data/oprabk /scalp/data/oprabk

COPY  startup /scalp/script/startup 
COPY startall /scalp/startall

COPY streaminterfaceapp /scalp/streaminterfaceapp
COPY quoteprotostream /scalp/quoteprotostream
COPY tradeprotostream /scalp/tradeprotostream 

COPY usr/local/lib /usr/local/lib

RUN chmod u+x /scalp/opnintdbapp && chmod u+x /scalp/opnintdb 
RUN chmod u+x /scalp/oprabk && chmod u+x /scalp/oprabk-c && chmod u+x /scalp/oprabkapp-c && chmod u+x /scalp/oprabkapp
RUN chmod u+x /scalp/oprasdb 
RUN chmod u+x /scalp/secudbapp
#RUN chmod u+x /scalp/optdb && 
RUN  chmod u+x /scalp/script/startup && chmod u+x /scalp/startall
RUN chmod u+x /scalp/streaminterfaceapp && chmod u+x /scalp/quoteprotostream && chmod u+x /scalp/tradeprotostream

RUN yum install -y wget ruby-2.0.0.648-30.el7.x86_64 
RUN gem install tiller 

COPY tiller /scalp/tiller

#ENV DOCKERIZE_VERSION v0.6.0
#RUN wget https://github.com/jwilder/dockerize/releases/download/$DOCKERIZE_VERSION/dockerize-linux-amd64-$DOCKERIZE_VERSION.tar.gz \
#    && tar -C /usr/local/bin -xzvf dockerize-linux-amd64-$DOCKERIZE_VERSION.tar.gz \
 #   && rm dockerize-linux-amd64-$DOCKERIZE_VERSION.tar.gz

#CMD ["tiller", "-b", "/scalp/tiller/quickstart", "-d", "-a", "-v","--environment", $(hostname)]


CMD tiller -b /scalp/tiller/quickstart -d -a -v --environment $(hostname)


#CMD scalp/oprabkapp-r -protobin -smbinsz 32 -avgbbosz 60 -avgsprd 60 -trdbk trdbk_opra -opnint opnintbk -secudb optdb -bk oprabk -trdstrm trdstrm_opra -qstrm qstrm_opra -fdiface 10.254.32.78 -feed 233.43.202.[1-24]:[11101-11124]

#CMD dockerize -template /etc/scalpel/oprabk.tmpl:/etc/scalpel/oprabk.conf -stdout /scalp/logs/oprabkapp-stdout.log -stderr /scalp/opra/logs/oprabkapp-stderr.log 
