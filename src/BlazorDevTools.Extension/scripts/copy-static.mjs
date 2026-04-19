import { cp, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const currentDirectory = path.dirname(fileURLToPath(import.meta.url));
const extensionRoot = path.resolve(currentDirectory, "..");
const sourceDirectory = path.join(extensionRoot, "static");
const destinationDirectory = path.join(extensionRoot, "dist");

await mkdir(destinationDirectory, { recursive: true });
await cp(sourceDirectory, destinationDirectory, { recursive: true });
