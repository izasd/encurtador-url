import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { QrCode } from './qr-code';

describe('QrCode', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    // O jsdom não implementa URLs de objeto.
    URL.createObjectURL = vi.fn(() => 'blob:http://localhost/qr-teste');
    URL.revokeObjectURL = vi.fn();

    await TestBed.configureTestingModule({
      imports: [QrCode],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function criar() {
    const fixture = TestBed.createComponent(QrCode);
    fixture.componentRef.setInput('codigo', 'abc123');
    fixture.componentRef.setInput('urlCurta', 'http://localhost:5198/abc123');
    return fixture;
  }

  it('busca o PNG na API e mostra a imagem com texto alternativo e o botão de baixar', async () => {
    const fixture = criar();
    const pagina = fixture.nativeElement as HTMLElement;

    const requisicao = await vi.waitFor(() => http.expectOne((r) => r.url === '/api/links/abc123/qrcode'));
    expect(requisicao.request.params.get('tamanho')).toBe('8');
    expect(requisicao.request.responseType).toBe('blob');
    requisicao.flush(new Blob([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], { type: 'image/png' }));

    await vi.waitFor(() => expect(pagina.querySelector('img')?.getAttribute('src')).toBe('blob:http://localhost/qr-teste'));
    expect(pagina.querySelector('img')?.getAttribute('alt')).toBe('QR Code que abre http://localhost:5198/abc123');
    expect(pagina.querySelector('a')?.getAttribute('download')).toBe('qrcode-abc123.png');
  });

  it('libera a URL da imagem quando o componente sai da tela', async () => {
    const fixture = criar();

    (await vi.waitFor(() => http.expectOne((r) => r.url === '/api/links/abc123/qrcode'))).flush(new Blob([]));
    await vi.waitFor(() => expect(URL.createObjectURL).toHaveBeenCalled());
    fixture.destroy();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:http://localhost/qr-teste');
  });
});
