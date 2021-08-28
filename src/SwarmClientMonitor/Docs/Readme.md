# Design Notes

Solution consists of:-

1. `oauth-client-credential` service for registering oauth clients for the `oauth client credentials flow` which will be used to service to service authentication.
2. `oauth-client-credentials-auto-registration` service, which can be notified when there are new services created or removed from the swarm, and will then ensure they are set up as clients in the `oauth-client-credential`. Will also ensure that any new clients that have to created, have their `client secret`'s made available to them as `docker secret`s. Likewise if a service is destroyed, it's `docker secret` will be destroyed.
3. `dockerflow/swarm-listener` service, for detecting when new services are created on destroyed on the swarm, and notifying the `oauth-client-credentials-auto-registration` service.

## oauth-client-credentials service

Has an api for adding and removing clients.

- /api/clients/add - add a new client with the specified `client id`, generates and returns a `client secret` which cannot be retreived again. If client already exists with same client id then a non successful status code is returned.
- /api/clients/remove - remove a client, also destroys its secret in the database.
- /api/cert - get the public cert used to validate authentication tokens.
- /api/token - exchange a `client id`, and `client secret` for a signed JWT.

We want `/api/client/` api's to be accessible to `oauth-client-credentials-auto-registration` only.
We want `/api/token` api's to be accessible to any service that needs to obtain auth tokens.
We want `/api/cert` api to be accessible to any service that needs to validate the signed JWT token.

We could split this service into seperate services called `oauth-clients` and `oauth-client-tokens` and map a shared volume so they can share information, like so:

```yaml
oauth-clients:
    image: oauth-client-credentials-clients   
    volumes:
      - clients:/clients:rw
    networks:   
      - service-management

oauth-clients-auto-registration:
    image: oauth-client-credentials-auto-registration      
    networks:   
      - service-management
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock # needs this so it can run `docker secret` commands.
    environment:
      - OAUTH_NOTIFY_NEW_SERVICE_URL=http://oauth-clients/clients/add
      - OAUTH_NOTIFY_REMOVE_SERVICE_URL=http://oauth-clients/clients/remove

swarm-listener:
    # SEE https://swarmlistener.dockerflow.com/config/ 
    image: dockerflow/docker-flow-swarm-listener
    networks:
      - service-management
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - DF_NOTIFY_CREATE_SERVICE_URL=http://oauth-clients-auto-registration/api/echo
      - DF_NOTIFY_REMOVE_SERVICE_URL=http://oauth-clients-auto-registration/api/echo
      - DF_NOTIFY_CREATE_SERVICE_METHOD=POST
      - DF_NOTIFY_REMOVE_SERVICE_METHOD=POST
      - DF_NOTIFY_CREATE_NODE_URL=http://oauth-clients-auto-registration/api/echo
      - DF_NOTIFY_REMOVE_NODE_URL=http://oauth-clients-auto-registration/api/echo

    deploy:
      placement:
        constraints: [node.role == manager]

oauth-client-tokens:
    image: oauth-client-credentials-client-tokens
    volumes:
      - clients:/clients:rw
    # networks:   
    #  - ingress # just use standard ingress network which is default if network not specified.
    ports:
      - 5001:80 # if you wanted to publish an external port, if just used for internal service to service communication this isnt necessary.

```

In this setup:-

- `oauth-client-tokens` service hosts the `/api/token` for getting an auth token. Its on the ingress network so should be accessible to all services.
  - It mounts the same volume as the `oauth-clients` service - because it needs access to the registered `clients` and their `client secrets`.
  - `client secrets` are stored in the volume as ordinary files, however they are protected at rest using `DPAPI`. `DPAPI` is configured the same in this service as well as the `oauth-client-tokens` service so they can both protect and unprotect files.

- `oauth-clients` service hosts the `/api/clients` api's for registering or removing clients. It's on the management network so it should not be accessible to services not on this network. The `oauth-clients-auto-registration` is on this network.

- `oauth-clients-auto-registration` service is on the `management` network so can call the `oauth-clients` service and register or remove oauth clients for new services that are created or destroyed in the swarm. When it adds new clients it gets back the client's `client secret` which it then provisions as a `docker secret` and maps to the service. The service now has access to the `secret` containing the `client secret` which it can then use to request JWT authentication tokens from the `oauth-client-tokens` `/api/token` endpoint.

## Alternative to writing our own oauth api

The mechanism for detecting new services and provsioning them as oauth clients seems nice.
However we have to write the actual oauth api where clients can be configured, and signed JWT tokens can be obtained. Should see if there is some existing service that can be leveraged for this.

Check these out:-

- [ory](https://www.ory.sh/run-oauth2-server-open-source-api-security/)

It should be possible to use ory:-
  - admin endpoint would not be exposed publicly.
  - `oauth-clients-auto-registration` container would use ory cli and be attached to same network as ory in order to perform admin functions like creating new clients.
  - other services would use ory's token endpoint.
