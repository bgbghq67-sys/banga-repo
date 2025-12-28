import { redirect } from "next/navigation";

export default function OtpPage() {
  // Redirect old /otp page to new dashboard
  redirect("/dashboard");
}
