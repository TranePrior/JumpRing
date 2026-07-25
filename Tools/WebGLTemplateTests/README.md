# WebGL template tests

Node tests for the JS layer of the WebGL template, which Unity's test framework cannot reach.

```bash
node Tools/WebGLTemplateTests/plugin-storage.test.js
```

`plugin-storage.test.js` covers the player-data storage in
`Assets/WebGLTemplates/PlatformLinkTemplate/TemplateData/plugin.js`: key merging, redundant
save suppression, and the guard against writing before the cloud data was loaded.
