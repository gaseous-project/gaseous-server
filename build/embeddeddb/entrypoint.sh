#!/bin/sh

# create the user
echo "Creating user gaseous with UID ${PUID} and GID ${PGID}"
getent group ${PGID} > /dev/null 2>&1 || groupadd -g ${PGID} gaseous

# Check if user with PUID exists
if id ${PUID} > /dev/null 2>&1; then
  # User exists, get its name and rename if necessary
  CURRENT_USER=$(id -un ${PUID})
  if [ "$CURRENT_USER" != "gaseous" ]; then
    usermod -l gaseous -d /home/gaseous "$CURRENT_USER"
  fi
else
  # User doesn't exist, create it
  useradd -u ${PUID} -g ${PGID} -m gaseous -d /home/gaseous -G sudo
fi
usermod -p "*" gaseous
mkdir -p /home/gaseous/.aspnet
chown -R ${PUID} /App /home/gaseous/.aspnet
chgrp -R ${PGID} /App /home/gaseous/.aspnet
mkdir -p /home/gaseous/.gaseous-server
chown -R ${PUID} /App /home/gaseous/.gaseous-server
chgrp -R ${PGID} /App /home/gaseous/.gaseous-server

# Set MariaDB permissions
mkdir -p /var/lib/mysql /var/log/mariadb /run/mysqld
chown -R ${PUID} /var/lib/mysql /var/log/mariadb /run/mysqld
chgrp -R ${PGID} /var/lib/mysql /var/log/mariadb /run/mysqld

# Start supervisord and services
/usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf