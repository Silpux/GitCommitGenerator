# Git commit generator

## Usage

1) Select desired year and hit `GetDays`. The calendar, similar to GitHub's commit history will be built.
2) Click on desired square, and add commits for that day. Each click it will add as many commits as you have in `Commit increment` text box. Left click - adds commits, right click - removes commits.
3) Input author name and email, or leave empty to use default. `GIT_COMMITTER_NAME` will be same as `GIT_AUTHOR_NAME`, and `GIT_COMMITTER_EMAIL` will be same as `GIT_AUTHOR_EMAIL`.
4) Select location of repository. If no `.git` folder is found, `git init` will be called first.
5) Click `Generate commits`, and commit history for your repository will be generated.