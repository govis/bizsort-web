export default function Template({ children }: { children: React.ReactNode }) {
  return (
    <div className="page-transition" style={{ display: 'flex', flexDirection: 'column', flex: 1 }}>
      {children}
    </div>
  );
}
