import { S3Client, GetObjectCommand, PutObjectCommand } from "@aws-sdk/client-s3";
import { getSignedUrl } from "@aws-sdk/s3-request-presigner";
import { serverEnv } from "./env";

const r2Endpoint = `https://${serverEnv.r2AccountId}.r2.cloudflarestorage.com`;

export const r2Client = new S3Client({
  region: "auto",
  endpoint: r2Endpoint,
  credentials: {
    accessKeyId: serverEnv.r2AccessKeyId,
    secretAccessKey: serverEnv.r2SecretAccessKey,
  },
});

export async function getSignedObjectUrl(key: string, expiresIn?: number) {
  const command = new GetObjectCommand({
    Bucket: serverEnv.r2Bucket,
    Key: key,
  });

  return getSignedUrl(r2Client, command, {
    expiresIn: expiresIn ?? serverEnv.signedUrlTtlSeconds,
  });
}

export async function uploadFile(key: string, buffer: Buffer, contentType: string) {
  const command = new PutObjectCommand({
    Bucket: serverEnv.r2Bucket,
    Key: key,
    Body: buffer,
    ContentType: contentType,
  });

  await r2Client.send(command);
  return key;
}

