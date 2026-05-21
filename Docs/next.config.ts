import { createMDX } from "fumadocs-mdx/next";
import type { NextConfig } from "next";

const withMDX = createMDX();

const nextConfig: NextConfig = {
    output: "standalone", //Reduces the size of the output
    reactStrictMode: true, // Enables React's Strict Mode
};

export default withMDX(nextConfig);
