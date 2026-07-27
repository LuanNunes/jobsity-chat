/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Emit a self-contained server (.next/standalone) for a lean production Docker image.
  output: "standalone",
};

export default nextConfig;
