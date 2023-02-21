# Dashboard

The dashboard project uses vue 2 to build with vite, and the UI uses bootstrap 4.

## Local Development

### Install dependent packages

> cd src\DotNetCore.CAP.Dashboard\wwwroot

```sh
npm install
```

### Update backend api

Update the `target` in `vite.config.js` of development to specify the backend service api.

```
  server: {
    proxy: {
      '^/cap/api': {
        target: 'http://localhost:5000',  //backend
        changeOrigin: true
      }
    }
  }
```

### Run

The backend api needs to allow cross-domain access.

```
npm run dev
```

## Publish

The release will be generated into the `dist` folder, and the contents of this folder will be embedded in the dashboard csproj assembly.

```
npm run build
```

