# CAP Documentation

The folder contains the documentation for CAP.

Documentation is built with the workflow in `.github/workflows/deploy-docs-and-dashboard.yml` and hosted on [GitHub Pages](https://cap.dotnetcore.xyz).

## Docs site

Doc pages are authored in Markdown  - you can find a primer [here](https://help.gamejolt.com/markdown).

Web site made with [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/)

### Local build with docker

```
cd CAP/docs
docker run --rm -it -p 8000:8000 -v ${PWD}:/docs squidfunk/mkdocs-material
```
