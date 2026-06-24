import { ESLint } from 'eslint';

const fix = process.argv.includes('--fix');
const maxWarnings = 0;

const eslint = new ESLint({ fix });
const results = await eslint.lintFiles(['.']);

if (fix) {
  await ESLint.outputFixes(results);
}

const formatter = await eslint.loadFormatter('stylish');
const resultText = formatter.format(results);

if (resultText.trim()) {
  console.log(resultText);
}

const errorCount = results.reduce((sum, result) => sum + result.errorCount, 0);
const warningCount = results.reduce((sum, result) => sum + result.warningCount, 0);
const fixableErrorCount = results.reduce((sum, result) => sum + result.fixableErrorCount, 0);
const fixableWarningCount = results.reduce((sum, result) => sum + result.fixableWarningCount, 0);
const filesWithIssues = results.filter(
  (result) => result.errorCount > 0 || result.warningCount > 0,
).length;

console.log('');
console.log(`Lint summary: ${results.length} files checked, ${filesWithIssues} with issues`);
console.log(`  Errors:   ${errorCount}${fixableErrorCount > 0 ? ` (${fixableErrorCount} fixable)` : ''}`);
console.log(`  Warnings: ${warningCount}${fixableWarningCount > 0 ? ` (${fixableWarningCount} fixable)` : ''}`);

if (errorCount > 0) {
  console.log('\nLint failed.');
  process.exit(1);
}

if (warningCount > maxWarnings) {
  console.log(`\nLint failed: ${warningCount} warning(s) exceed limit of ${maxWarnings}.`);
  process.exit(1);
}

console.log('\nLint passed.');
