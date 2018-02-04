#!/bin/bash

echo ">>> Kernel: $(uname -r)"
echo ">>> Updating system to 7.3.1611"
sed -i -e 's/^mirrorlist=/#mirrorlist=/' -e 's/^#baseurl=/baseurl=/' /etc/yum.repos.d/CentOS-Base.repo
yum -y --releasever=7.3.1611 update
sed -i -e 's/^#mirrorlist=/mirrorlist=/' -e 's/^baseurl=/#baseurl=/' /etc/yum.repos.d/CentOS-Base.repo


yum  update -y  
yum install -y httpd httpd-devel cmake vim git curl glibc-headers gcc-c++ autoconf automake libtool unzip make gdb selinux-policy-targeted


echo ">>> Installing DC/OS dependencies and essential packages"
yum -y --tolerant install sudo perl tar xz unzip curl bind-utils net-tools ipset libtool-ltdl rsync nfs-utils systemd systemctl


echo ">>> Disabling SELinux"
sed -i 's/SELINUX=enforcing/SELINUX=permissive/g' /etc/selinux/config
echo ">>> Adjusting SSH Daemon Configuration"

sed -i '/^\s*PermitRootLogin /d' /etc/ssh/sshd_config
echo -e "\nPermitRootLogin without-password" >> /etc/ssh/sshd_config

sed -i '/^\s*UseDNS /d' /etc/ssh/sshd_config
echo -e "\nUseDNS no" >> /etc/ssh/sshd_config

echo ">>> Disable rsyslog"
systemctl disable rsyslog

echo ">>> Set journald limits"
mkdir -p /etc/systemd/journald.conf.d/
echo -e "[Journal]\nSystemMaxUse=15G" > /etc/systemd/journald.conf.d/dcos-el7-ami.conf

echo ">>> Removing tty requirement for sudo"
sed -i'' -E 's/^(Defaults.*requiretty)/#\1/' /etc/sudoers

sudo systemctl stop firewalld && sudo systemctl disable firewalld

echo ">>> Install Docker"
curl -fLsSv --retry 20 -Y 100000 -y 60 -o /tmp/docker-engine-1.13.1-1.el7.centos.x86_64.rpm \
  https://yum.dockerproject.org/repo/main/centos/7/Packages/docker-engine-1.13.1-1.el7.centos.x86_64.rpm
curl -fLsSv --retry 20 -Y 100000 -y 60 -o /tmp/docker-engine-selinux-1.13.1-1.el7.centos.noarch.rpm \
  https://yum.dockerproject.org/repo/main/centos/7/Packages/docker-engine-selinux-1.13.1-1.el7.centos.noarch.rpm
yum -y localinstall /tmp/docker*.rpm || true
systemctl enable docker

echo ">>> Creating docker group"
/usr/sbin/groupadd -f docker
echo ">>> Customizing Docker storage driver to use Overlay"
docker_service_d=/etc/systemd/system/docker.service.d
mkdir -p "$docker_service_d"
cat << 'EOF' > "${docker_service_d}/execstart.conf"
[Service]
Restart=always
StartLimitInterval=0
RestartSec=15
ExecStartPre=-/sbin/ip link del docker0
ExecStart=
ExecStart=/usr/bin/dockerd --graph=/var/lib/docker --storage-driver=overlay
EOF

echo ">>> Adding group [nogroup]"
/usr/sbin/groupadd -f nogroup

systemctl daemon-reload
sync
