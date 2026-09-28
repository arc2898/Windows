const rules = require('./webpack.rules');

module.exports = {
  entry: './src/renderer/index.tsx',
  // Put your normal webpack config below here
  module: {
    rules,
  },
  resolve: {
    extensions: ['.js', '.ts', '.jsx', '.tsx', '.json'],
  },
};
