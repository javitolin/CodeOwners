# Readme
## Purpose
* Enforce code ownership on different parts of the project
* Specially helpful for MonoRepo microservice based system

## Process
* Get all opened PRs
* `git pull` the destination branch
* Parse the CODEOWNERS file (from DESTINATION branch)
* `git pull` the source branch
* Find changes between source and destination branches
* Find code owners for the changes and add them to the PR as revieweres
    * If added, send them a Rocket.Chat message

## CODEOWNER file
* Very much like GitHub's CODEOWNER file
* Should be placed in the root of your repo, called CODEOWNERS
* You can use "#" for comments
* Order of the items is important, last one wins
* Can use "*" for wildcards