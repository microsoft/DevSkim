module.exports = {
  preset: 'ts-jest',
  roots: ['<rootDir>/src'],
  testEnvironment: 'node',
  testPathIgnorePatterns: [
      "<rootDir>/node_modules/",
      "<rootDir>/out/"
  ]
};