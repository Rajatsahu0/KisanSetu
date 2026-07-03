import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { MobileNav } from './MobileNav';
import { Footer } from './Footer';
import { Breadcrumb } from './Breadcrumb';
import { PageTransition } from '@/components/navigation/PageTransition';

export const AppLayout: React.FC = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen);
  };

  const closeMobileMenu = () => {
    setIsMobileMenuOpen(false);
  };

  return (
    <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-900 overflow-x-hidden max-w-[100vw]">
      <Header onMenuToggle={toggleMobileMenu} isMobileMenuOpen={isMobileMenuOpen} />
      
      <MobileNav isOpen={isMobileMenuOpen} onClose={closeMobileMenu} />
      
      <main 
        id="main-content" 
        className="flex-1 px-2 sm:px-4 lg:px-6 py-4 md:py-6"
        role="main"
        aria-label="Main content"
      >
        <Breadcrumb />
        <PageTransition>
          <Outlet />
        </PageTransition>
      </main>
      
      <Footer />
    </div>
  );
};
