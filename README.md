# Tavern

_Where all of Sticky's members come together to participate in fun activities._

## Description

Sticky hosts an abundance of activities. This is where we manage them.

## How to install

TL;DR clone repo

## How to use

TL;DR use the devcontainer, using your favourite tool like vscode or Jetbrains Rider

## How to host

See HOSTING.md

## Architecture

See ARCHITECTURE.md

## How to contribute

Some stuff, but also see ARCHITECTURE.md

## Keycloak
For keycloak:
Create a client with authentication and authorization on, take the secret key in the credentials tap and add it to the env, go to client scopes of the client and select the dedicated one, add an user_attribute mapper with as attribute access_level and leave the settings like it is. Do the same for koala_user_id and member_memberships but for member_memberships turn on multivalued.

For applications that should not be accassible for notpaid or suspended accounts:
Go back to the client, go to authorization>policies
Add a regex policy with target claim access_level and regex ^notpaid$ and logic negative
Do the same but with regex ^suspended$.
Go to Resources and create one with:
Name: Default Resource
Type: urn:backend:resource:default
URI's: /*
Go to permissions-> Create Permission and select the created resource and one of the policies, leave the other settings.
To the same for the other policy in permissions.
