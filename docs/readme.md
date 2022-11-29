# CAP Documentation

The folder contains the documentation for CAP.

We are using [Github Pages](https://github.com/dotnetcore/CAP/tree/gh-pages) to host the documentation and the rendered version can be found [here](http://cap.dotnetcore.xyz).

## Docs site

Doc pages are authored in Markdown  - you can find a primer [here](https://help.gamejolt.com/markdown).

Web site made with [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/)

### Local build with docker

```
cd CAP/docs
docker run --rm -it -p 8000:8000 -v ${PWD}:/docs squidfunk/mkdocs-material      
```