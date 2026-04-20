import { useState } from 'react';

interface CopyLinkButtonProps {
  url: string;
  label?: string;
}

export default function CopyLinkButton({ url, label = 'Copy Link' }: CopyLinkButtonProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Fallback for older browsers
      const textarea = document.createElement('textarea');
      textarea.value = url;
      document.body.appendChild(textarea);
      textarea.select();
      document.execCommand('copy');
      document.body.removeChild(textarea);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <button type="button" className="secondary outline" onClick={handleCopy}>
      {copied ? 'Copied!' : label}
    </button>
  );
}
