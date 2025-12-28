const requiredServerVars = [
  "R2_ACCESS_KEY_ID",
  "R2_SECRET_ACCESS_KEY",
  "R2_ACCOUNT_ID",
  "R2_BUCKET",
] as const;

type RequiredServerVar = (typeof requiredServerVars)[number];

function getEnv(name: RequiredServerVar) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing environment variable: ${name}`);
  }
  return value;
}

export const serverEnv = {
  r2AccessKeyId: getEnv("R2_ACCESS_KEY_ID"),
  r2SecretAccessKey: getEnv("R2_SECRET_ACCESS_KEY"),
  r2AccountId: getEnv("R2_ACCOUNT_ID"),
  r2Bucket: getEnv("R2_BUCKET"),
  r2PublicBase: process.env.R2_PUBLIC_BASE ?? "",
  signedUrlTtlSeconds: Number(process.env.R2_SIGNED_URL_TTL ?? 60 * 60),
};

export const publicEnv = {
  appUrl: process.env.NEXT_PUBLIC_APP_URL ?? "",
  firebase: {
    apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY ?? "",
    authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN ?? "",
    projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID ?? "",
    storageBucket: process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET ?? "",
    messagingSenderId:
      process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID ?? "",
    appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID ?? "",
  },
};

