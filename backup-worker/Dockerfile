FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive

# Install core utilities and setup MS repository key
RUN apt-get update && apt-get install -y curl gnupg2 git ca-certificates && \
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl https://packages.microsoft.com/config/ubuntu/22.04/prod.list > /etc/apt/sources.list.data/mssql-release.list

# Accept license agreement automatically and install sqlcmd tools
RUN apt-get update && ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev

# Copy backup execution engine
WORKDIR /app
COPY backup.sh .
RUN chmod +x backup.sh

ENTRYPOINT ["/bin/bash", "./backup.sh"]