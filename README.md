<p align="center">
  <img src="https://github.com/bitwarden/brand/blob/master/screenshots/apps-combo-logo.png" alt="Bitwarden" />
</p>
<p align="center">
  <a href="https://github.com/bitwarden/server/actions/workflows/build.yml?query=branch:master" target="_blank">
    <img src="https://github.com/bitwarden/server/actions/workflows/build.yml/badge.svg?branch=master" alt="Github Workflow build on master" />
  </a>
  <a href="https://hub.docker.com/u/bitwarden/" target="_blank">
    <img src="https://img.shields.io/docker/pulls/bitwarden/api.svg" alt="DockerHub" />
  </a>
  <a href="https://gitter.im/bitwarden/Lobby" target="_blank">
    <img src="https://badges.gitter.im/bitwarden/Lobby.svg" alt="gitter chat" />
  </a>
</p>

-------------------

The Bitwarden Server project contains the APIs, database, and other core infrastructure items needed for the "backend" of all bitwarden client applications.

The server project is written in C# using .NET Core with ASP.NET Core. The database is written in T-SQL/SQL Server. The codebase can be developed, built, run, and deployed cross-platform on Windows, macOS, and Linux distributions.

## Build/Run

Please read the [Setup guide](https://github.com/bitwarden/server/blob/master/SETUP.md) for a step-by-step guide to set up your own local development server.

### Requirements

- [.NET 5.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2017](https://docs.microsoft.com/en-us/sql/index)

*These dependencies are free to use.*

### Recommended Development Tooling

- [Visual Studio](https://www.visualstudio.com/vs/) (Windows and macOS)
- [Visual Studio Code](https://code.visualstudio.com/) (other)

*These tools are free to use.*

### API

```
cd src/Api
dotnet restore
dotnet build
dotnet run
```

visit http://localhost:4000/alive

### Identity

```
cd src/Identity
dotnet restore
dotnet build
dotnet run
```

visit http://localhost:33657/.well-known/openid-configuration

## Deploy

<p align="center">
  <a href="https://hub.docker.com/u/bitwarden/" target="_blank">
    <img src="https://i.imgur.com/SZc8JnH.png" alt="docker" />
  </a>
</p>

You can deploy Bitwarden using Docker containers on Windows, macOS, and Linux distributions. Use the provided PowerShell and Bash scripts to get started quickly. Find all of the Bitwarden images on [Docker Hub](https://hub.docker.com/u/bitwarden/).

Full documentation for deploying Bitwarden with Docker can be found in our help center at: https://help.bitwarden.com/article/install-on-premise/

### Requirements

- [Docker](https://www.docker.com/community-edition#/download)
- [Docker Compose](https://docs.docker.com/compose/install/) (already included with some Docker installations)

*These dependencies are free to use.*

### Linux & macOS

```
curl -s -o bitwarden.sh \
    https://raw.githubusercontent.com/bitwarden/server/master/scripts/bitwarden.sh \
    && chmod +x bitwarden.sh
./bitwarden.sh install
./bitwarden.sh start
```

### Windows

```
Invoke-RestMethod -OutFile bitwarden.ps1 `
    -Uri https://raw.githubusercontent.com/bitwarden/server/master/scripts/bitwarden.ps1
.\bitwarden.ps1 -install
.\bitwarden.ps1 -start
```

## We're Hiring!

Interested in contributing in a big way? Consider joining our team! We're hiring for many positions. Please take a look at our [Careers page](https://bitwarden.com/careers/) to see what opportunities are currently open as well as what it's like to work at Bitwarden.

## Contribute

Code contributions are welcome! Visual Studio or VS Code is highly recommended if you are working on this project. Please commit any pull requests against the `master` branch. Please see [`CONTRIBUTING.md`](CONTRIBUTING.md) for more info (and feel free to contribute to that guide as well).

Security audits and feedback are welcome. Please open an issue or email us privately if the report is sensitive in nature. You can read our security policy in the [`SECURITY.md`](SECURITY.md) file. We also run a program on [HackerOne](https://hackerone.com/bitwarden).

No grant of any rights in the trademarks, service marks, or logos of Bitwarden is made (except as may be necessary to comply with the notice requirements as applicable), and use of any Bitwarden trademarks must comply with [Bitwarden Trademark Guidelines](https://github.com/bitwarden/server/blob/master/TRADEMARK_GUIDELINES.md).

### Dotnet-format

We recently migrated to using dotnet-format as code formatter. All previous branches will need to updated to avoid large merge conflicts using the following steps:

1. Check out your local Branch
2. Run `git merge 61dc65aa598b1f492d2f0222bb7bf0dd15d116f5`
3. Resolve any merge conflicts, commit.
4. Run `dotnet tool run dotnet-format`
5. Commit
6. Run `git merge -Xours 23b0a1f9df25058ab29785ecad9a233113c10889`
7. Push

#### Git blame

We also recommend that you configure git to ignore the prettier revision using:

```bash
git config blame.ignoreRevsFile .git-blame-ignore-revs
```
