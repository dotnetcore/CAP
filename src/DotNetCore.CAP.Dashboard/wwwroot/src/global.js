let baseURL = "";

switch (process.env.NODE_ENV) {
    case 'development':
        baseURL = 'http://localhost:5001';
        break
    default:
        baseURL = window.serverUrl;
        break
}

export default baseURL;