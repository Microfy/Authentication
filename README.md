## What

"Service to Service" authentication for your docker swarm microservices - solved.

## Context and Motivation

Teams getting started with docker swarm might start with a couple of simple microservices, perhaps built and deployed by seperate teams to the swarm. 
Services: `foo` and `bar` for example.
They decide that they want `foo` to be able to call `bar` via it's http api, but because they also security in mind, they realise that they only want to allow authenticated calls between `foo` and `bar`.
Typically they might come accross TLS Client Certificates and decide that every backend service should be using a client certificate when making api calls.
Likewise all microservices that receive web api calls, should be ensuring the caller has a valid client certificate. 
This is know as mutual TLS or mTLS and is a common solution to this problem.

However TLS is not free. There is:-

- Overhead in performing the handshake.for each request.
- Issue of how client applications should manage certificates. Should it be added to source control? Mounted from an external volume? What about if it expires? Does it need to be rotated? Perhaps it should be downloaded every day from some source? Applications should also make sure to authenticate incoming http requests to check the client certificate is valid. This is a cross cutting concern.
- It does not allow for claims based authorization.

The `OAuth` specification has an alternative flow to solve this problem. It's called the `client credentials` flow.
Microfy aims to provide a couple of services that can easily be leveraged in your docker swarm deployments, to take care of this cross cutting concern of service to service authentication. 
These services are designed to make it easy for you to maintain and manage authentication with as little effort as required, whilst ensuring that that application developers don't have to spend any significant time repeatedly addressing this concern in their applications, allowing them to focus more on business logic.


## Authentication Server Service

An Authentication Server, that implements the OAuth "Client Credentials" flow is the first service.
This service is deployed to your swarm as a `global` service, on a published port in "host" mode.
This lets any service in the swarm, make an api call to this Authentication Server service, via a "localhost" https address on a standard port (i.e 445 or whatever you decide).
Services can call a /token endpoint to exchange a "client id" and a "client secret" for an oauth bearer JWT token.
This JWT token can then be used in the HTTP authorization header when making api calls to other backend services in the swarm. 
The Authentication Server can run on HTTP or HTTPS depending on whether you want to ensure that client secrets that are sent to it, remain secret from snoping. Although, its less critical in this scenario as 
traffic should not actually leave the host due to the AS running essentially as side car the same node. If your host node is compromised and someone is snooping on internal traffic, you already have a large issue at this point, and SSL of client secrets is probably not going to help.

## Authentication Gateway Service
An Authentication Gateway (or "Reverse Proxy") service that can be deployed "in front" of any microservice, and does JWT authentication before forwarding the request. This can be deployed in a number of ways:-

1. Each team that `docker stack deploys` their `docker compose` file to the swarm, can include this gateway service in their compose file.
They don't attach their api service to the docker `ingress` network directly, instead they attach this gateway service to the ingress network, and also define a private network that is atteched to the proxy and their api services.
The proxy will ensure all requests through the ingress network are authenticated before being forwarded to their api services over the private network. 
This means the only way for one teams microservice to call another teams microservice, isby sending a request through the gateway, ensuring JWT authentication takes place.

2. The second option, is that the Gateway service is deployed once as a global service, in host mode so that it essentially is a sidecar present on each node in the swarm, and accessible over a well known local port such as localhost:8082 etc. 
In this mode, teams that deploy compose files containing services to the swarm, again, do not attach them to the ingress network, but instead attach them to privately named networks the same as option 1.
However the gateway service no longer needs to be part of their compose file definitions.
The proxy service is then updated out of band, to be "attached" to the same private network which the teams microservices are published on. This makes those services "accessible" to other backend services in the swarm via the gateway, ensuring all requests are authenticated.
