export default function Loading() {
  return (
    <div style={{ position: 'fixed', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'rgba(255, 255, 255, 0.8)', zIndex: 9999 }}>
      <img 
        src="/images/bizsort-logo.svg" 
        alt="Bizsort logo" 
        style={{ width: '100px', height: '100px', position: 'absolute', top: '50%', left: '50%', transform: 'translateY(-50%) translateX(-50%)' }} 
      />
    </div>
  );
}
