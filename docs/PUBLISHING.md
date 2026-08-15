# Publishing Medlen to APT with Cloudsmith

This repository's release workflow publishes Medlen as a Debian package to a
public Cloudsmith repository. Cloudsmith signs and hosts the APT repository, so
users can install Medlen with the standard `apt install medlen` command.

## One-time setup

1. Create a Cloudsmith account or organization.
2. Create a **public** repository named `medlen` with Debian package support.
3. Create a Cloudsmith API key with permission to upload packages to that
   repository. Keep this key private.
4. In the GitHub repository, open **Settings → Secrets and variables →
   Actions** and add:

   | Type | Name | Value |
   | --- | --- | --- |
   | Secret | `CLOUDSMITH_API_KEY` | The Cloudsmith upload API key. |
   | Variable | `CLOUDSMITH_REPOSITORY` | `sorooshb/medlen` |


## Release

Tag a version and push the tag:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The GitHub Actions workflow then:

1. builds a self-contained 64-bit Debian package;
2. adds it to the corresponding GitHub Release; and
3. uploads it to Cloudsmith.

## User installation

After the first package is published, users install the Cloudsmith repository
configuration once, then use APT normally:

```bash
curl -fsSL 'https://dl.cloudsmith.io/public/sorooshb/medlen/cfg/setup/bash.deb.sh' \\
  | sudo env distro=ubuntu codename=any-version bash
sudo apt update
sudo apt install medlen
```

Cloudsmith shows the canonical setup command on each repository's **Set Me Up**
page. Use that generated command in the README, because it will always match
the repository's current signing configuration.
