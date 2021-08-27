# Swarm listener

The swarm listener runs as a docker container and is designed to listen to the following `docker swarm` events and send an HTTP notification to some configured URL when events are detected with more information:-

- Service created
- Service destroyed
- Node created
- Node destroyed

## Service Created

When a service is created on the swarm, for example using:-

```cmd
docker service create --name dummy-nginx --network monitor --label com.df.notify=true --label com.df.madeup=foo nginx
```

The swarm listener sends a request with the following content:

```http
Date received: 23:04:56
POST /api/echo?distribute=true&madeup=foo&replicas=1&serviceName=dummy-nginx HTTP/1.1

Accept-Encoding: gzip
Host: dummy-endpoint:80
User-Agent: Go-http-client/1.1
Content-Length: 0
```

## Service Scaled

If a service is scaled, for example using:-

```cmd
docker service scale dummy-nginx=2  
```

The swarm listener sends a request like this:-

```http
Date received: 23:13:18
POST /api/echo?distribute=true&madeup=foo&replicas=2&serviceName=dummy-nginx HTTP/1.1

Accept-Encoding: gzip
Host: dummy-endpoint:80
User-Agent: Go-http-client/1.1
Content-Length: 0
```

Note: the `replicas=2` query string parameter.

## Service Removed

If a service is scaled, for example using:-

```cmd
docker service rm dummy-nginx
```

The swarm listener sends a request like this:-

```http
Date received: 23:24:32
POST /api/echo?distribute=true&madeup=foo&serviceName=dummy-nginx HTTP/1.1

Accept-Encoding: gzip
Host: dummy-endpoint:80
User-Agent: Go-http-client/1.1
Content-Length: 0
```

Note: There is now no `replicas=` query string parameter, and this is the indicator that the service no longer exists.
