#!/bin/sh

# create the user
echo "Creating user gaseous with UID ${PUID} and GID ${PGID}"
groupadd -g ${PGID} gaseous
useradd -u ${PUID} -g ${PGID} -m gaseous -d /home/gaseous -G sudo 
usermod -p "*" gaseous
mkdir -p /home/gaseous/.gaseous-server /var/lib/mysql /var/log/mariadb /run/mysqld
chown -R ${PUID} /App /home/gaseous/.gaseous-server /var/lib/mysql /var/log/mariadb /run/mysqld
chgrp -R ${PGID} /App /home/gaseous/.gaseous-server /var/lib/mysql /var/log/mariadb /run/mysqld

# Start supervisord and services
/usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf