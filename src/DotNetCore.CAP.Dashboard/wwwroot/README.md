# Dashboard

The dashboard project uses vue 2 to build, and the UI uses bootstrap 4.

## Local Development

### Install dependent packages

> cd src\DotNetCore.CAP.Dashboard\wwwroot

```sh
npm install -g @vue/cli @vue/cli-service-global

npm install
```

### Update backend api

Update the `baseURL` in `global.js` of development to specify the backend service api.

```
switch (process.env.NODE_ENV) {
    case 'development':
        baseURL = 'http://localhost:5000';  // backend api
        break
    default:
        baseURL = window.serverUrl;
        break
}
```

### Run

The backend api needs to allow cross-domain access.

```
npm run serve
```

## Publish

The release will be generated into the `dist` folder, and the contents of this folder will be embedded in the dotnet csproj assembly.

```
npm run build
```

