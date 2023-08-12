# Readme
## Purpose
* Enforce code ownership on different parts of the project
* Specially helpful for MonoRepo microservice based system

## Process
1. Get all opened PRs
1. `git pull` the destination branch
1. Parse the CODEOWNERS file (from DESTINATION branch)
1. `git pull` the source branch
1. Find changes between source and destination branches
1. Find code owners for the changes and add them to the PR as revieweres
    * If added, send them a notification

## CODEOWNER file
* Very much like GitHub's CODEOWNER file
* Should be placed in the root of your repo, file name: CODEOWNERS
* You can use "#" for comments
* Order of the items is important, last one wins
* Use "*" followed by usernames for "default owners"

### CODEOWNER example:
```
# Default codeowner for the project
* @project_owner

# Specific owner for changes on "service_1/" directory (and everything inside)
service_1/ @service_1_owner
```

## Currently implemented
* Pull Request source:
    * Azure DevOps

* Notifications:
    * RocketChat

## Notifier Message Format
* Can use any of these:
    * {username}: RocketChat username
    * {pr_url}: PullRequest URL
    * {pr_name}: PullRequest Name
    * {pr_description}: PullRequest Description
    * {pr_id}: PullRequest unique id
    * {pr_reviewers}: List of reviewers, separated by comma
    * {pr_destination_branch}: PullRequest Destionation branch
    * {pr_source_branch}: PullRequest Source branch
    * {pr_repository}: PullRequest repository

## Contributing
1. Fork it (<https://github.com/javitolin/CodeOwners/fork>)
2. Create your feature branch (`git checkout -b feature/fooBar`)
3. Commit your changes (`git commit -am 'Add some fooBar'`)
4. Push to the branch (`git push origin feature/fooBar`)
5. Create a new Pull Request

## Meta
* [AsadoDevCulture](https://AsadoDevCulture.com) 
* [@jdorfsman](https://twitter.com/jdorfsman)
* Distributed under the MIT license.