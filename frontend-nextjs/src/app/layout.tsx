import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import "@awesome.me/webawesome/dist/styles/themes/default.css";
import SignInFormStub from "../components/global/SignInFormStub";
import MessageToastStub from "../components/global/MessageToastStub";
import NavigationProvider from "../components/global/NavigationProvider";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: {
    template: '%s | BizSort',
    default: 'BizSort - Business Directory',
  },
  description: 'BizSort — find and explore local businesses, view company profiles, products, services, and more.',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable}`}
      style={{ height: '100%' }}
    >
      <body style={{ minHeight: '100%', display: 'flex', flexDirection: 'column', margin: 0 }}>
        {children}
        <SignInFormStub />
        <MessageToastStub />
        <NavigationProvider />
      </body>
    </html>
  );
}
