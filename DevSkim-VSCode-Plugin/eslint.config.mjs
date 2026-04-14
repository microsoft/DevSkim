import eslint from "@eslint/js";
import tseslint from "typescript-eslint";

export default tseslint.config(
	eslint.configs.recommended,
	...tseslint.configs.recommended,
	{
		files: ["client/**/*.ts"],
		rules: {
			"semi": [2, "always"],
			"@typescript-eslint/no-unused-vars": "off",
			"@typescript-eslint/no-explicit-any": "off",
			"@typescript-eslint/no-non-null-assertion": "off",
		},
	},
	{
		ignores: ["**/out/**", "**/node_modules/**", "scripts/**"],
	}
);
